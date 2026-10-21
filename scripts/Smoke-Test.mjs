// Run after dotnet build: node scripts/Smoke-Test.mjs
// Starts an isolated development server and removes only its temporary test data.
import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';
import { createServer } from 'node:net';

const root = fileURLToPath(new URL('../', import.meta.url));
const directory = await mkdtemp(join(tmpdir(), 'sportsbooking-test-'));
const listener = createServer();
await new Promise(resolve => listener.listen(0, '127.0.0.1', resolve));
const port = listener.address().port;
await new Promise(resolve => listener.close(resolve));
const base = `http://127.0.0.1:${port}`;
const password = `Test!${randomUUID()}Aa1`;
let logs = '';
const server = spawn('dotnet', ['run', '--project', resolve(root, 'SportsBooking.Web'),
  '--no-build', '--no-launch-profile', '--urls', base], {
  cwd: root, windowsHide: true,
  env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ENVIRONMENT: 'Development',
    Database__Provider: 'Sqlite',
    ConnectionStrings__SportsBooking: `Data Source=${join(directory, 'bookings.db')}`,
    ConnectionStrings__Identity: `Data Source=${join(directory, 'identity.db')}`,
    SPORTSBOOKING_ADMIN_EMAIL: 'admin@smoke.example', SPORTSBOOKING_ADMIN_PASSWORD: password,
    Logging__LogLevel__Default: 'Warning' }
});
server.stdout.on('data', data => { logs += data; });
server.stderr.on('data', data => { logs += data; });
server.on('error', error => { logs += error.message; });

class Browser {
  cookies = new Map();
  async request(path, data) {
    const headers = { Cookie: [...this.cookies].map(([k, v]) => `${k}=${v}`).join('; ') };
    if (data) headers['Content-Type'] = 'application/x-www-form-urlencoded';
    const response = await fetch(base + path, { method: data ? 'POST' : 'GET',
      headers, body: data ? new URLSearchParams(data) : undefined, redirect: 'manual' });
    for (const cookie of response.headers.getSetCookie()) {
      const pair = cookie.split(';')[0];
      this.cookies.set(pair.slice(0, pair.indexOf('=')), pair.slice(pair.indexOf('=') + 1));
    }
    return { status: response.status, location: response.headers.get('location'), text: await response.text() };
  }
  async page(path) {
    const response = await this.request(path);
    assert.equal(response.status, 200, `${path}: ${response.status}\n${response.text.slice(0, 500)}`);
    return response.text;
  }
  async post(path, data, formPath = path) {
    const html = await this.page(formPath);
    const token = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
    assert.ok(token, `Antiforgery token on ${formPath}`);
    return this.request(path, { ...data, __RequestVerificationToken: token[1] });
  }
}

try {
  let ready = false;
  for (let attempt = 0; attempt < 120; attempt++) {
    if (server.exitCode !== null) throw new Error(`Server exited: ${server.exitCode}`);
    try { ready = (await fetch(base)).ok; } catch { /* Waiting for startup. */ }
    if (ready) break;
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  assert.ok(ready, 'Server starts within 30 seconds');
  const guest = new Browser();
  for (const path of ['/', '/Fields', '/Fields/Details/1', '/Sports', '/Sports/Details/1',
    '/SportsCenters', '/SportsCenters/Details/1', '/Account/Login', '/Account/Register',
    '/css/site.css', '/js/site.js']) await guest.page(path);
  assert.equal((await guest.request('/Bookings')).status, 302);
  assert.equal((await guest.request('/Account/Login', {})).status, 400);
  console.log('PASS: public pages, assets, login requirement, antiforgery protection');

  const user = new Browser();
  let response = await user.post('/Account/Register', {
    FullName: 'Smoke Test User', Email: 'user@smoke.example', Phone: '0888111111',
    Password: password, ConfirmPassword: password
  });
  assert.equal(response.status, 302, 'Registration succeeds');
  await user.page('/Bookings');
  const date = new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10);
  const booking = { FieldID: '1', BookingDate: date, StartTime: '12:00', EndTime: '13:00' };
  response = await user.post('/Bookings/Create', booking);
  assert.equal(response.status, 302, `Booking succeeds: ${response.text}`);
  const listing = await user.page('/Bookings');
  const bookingId = listing.match(/\/Bookings\/Details\/(\d+)/)?.[1];
  assert.ok(bookingId, 'Created booking appears in list');
  await user.page(`/Bookings/Details/${bookingId}`);
  response = await user.post('/Bookings/Create', booking);
  assert.equal(response.status, 200, 'Overlapping booking is rejected');
  assert.match(response.text, /data-valmsg-for="StartTime"[^>]*>[\s\S]*?field-validation-error|field-validation-error[^>]*data-valmsg-for="StartTime"/);
  response = await user.post('/Bookings/Create', { ...booking, EndTime: '11:00' });
  assert.equal(response.status, 200, 'Invalid time range rejected');
  response = await user.post('/Bookings/Create', { ...booking,
    BookingDate: new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10) });
  assert.equal(response.status, 200, 'Past date rejected');
  assert.equal((await user.request('/Users')).status, 302, 'Regular user cannot access admin pages');
  console.log('PASS: registration, HTTP authentication, booking creation and validation');

  const other = new Browser();
  assert.equal((await other.post('/Account/Register', { FullName: 'Other User', Email: 'other@smoke.example',
    Password: password, ConfirmPassword: password })).status, 302);
  assert.doesNotMatch(await other.page('/Bookings'), new RegExp(`/Bookings/Details/${bookingId}(?:"|\\?)`));
  assert.match((await other.request(`/Bookings/Details/${bookingId}`)).location, /AccessDenied/);
  assert.match((await other.post(`/Bookings/Cancel/${bookingId}`, {}, '/Bookings')).location, /AccessDenied/);
  assert.match(await user.page(`/Bookings/Details/${bookingId}`), /12:00/);
  console.log('PASS: reservation ownership enforcement');

  assert.equal((await user.post('/Account/Logout', {}, '/')).status, 302);
  assert.equal((await user.request('/Bookings')).status, 302);
  response = await user.post('/Account/Login?returnUrl=%2FBookings', { Email: 'user@smoke.example', Password: 'wrong' });
  assert.equal(response.status, 200);
  assert.match(response.text, /returnUrl=%2FBookings/i, 'Return URL survives failed login');
  response = await user.post('/Account/Login?returnUrl=%2FBookings', { Email: 'user@smoke.example', Password: password });
  assert.equal(response.status, 302);
  assert.equal(response.location, '/Bookings');
  assert.equal((await user.post(`/Bookings/Cancel/${bookingId}`, {}, '/Bookings')).status, 302);
  assert.equal((await user.post('/Bookings/Create', booking)).status, 302, 'Cancelled slot can be rebooked');
  console.log('PASS: logout, failed login, return URL, login, cancellation and rebooking');

  const admin = new Browser();
  assert.equal((await admin.post('/Account/Login', { Email: 'admin@smoke.example', Password: password })).status, 302);
  for (const controller of ['Sports', 'SportsCenters', 'Fields', 'Users', 'Bookings', 'Payments']) {
    await admin.page(`/${controller}`);
    await admin.page(`/${controller}/Create`);
  }
  await admin.page('/Roles');
  for (const controller of ['Sports', 'SportsCenters', 'Fields', 'Users', 'Bookings']) {
    for (const action of ['Details', 'Edit', 'Delete']) await admin.page(`/${controller}/${action}/1`);
  }
  assert.equal((await admin.post('/Payments/Create', {
    BookingID: bookingId, Amount: '40.00', PaymentMethod: 'Карта'
  })).status, 302);
  for (const action of ['Details', 'Edit', 'Delete']) await admin.page(`/Payments/${action}/1`);
  assert.equal((await admin.post('/Payments/Edit/1', {
    BookingID: bookingId, Amount: '45.00', PaymentMethod: 'В брой'
  })).status, 302);
  assert.equal((await admin.post('/Payments/Delete/1', {})).status, 302);
  assert.equal((await admin.post('/Sports/Create', { SportName: 'Smoke Test Sport' })).status, 302);
  const sports = await admin.page('/Sports');
  const sportId = sports.match(/Smoke Test Sport[\s\S]*?\/Sports\/Details\/(\d+)/)?.[1];
  assert.ok(sportId);
  assert.equal((await admin.post(`/Sports/Edit/${sportId}`, { SportName: 'Updated Test Sport' })).status, 302);
  assert.equal((await admin.post(`/Sports/Delete/${sportId}`, {})).status, 302);
  console.log('PASS: admin pages, sport CRUD, payment creation/edit/delete');
  console.log('All smoke checks passed. Test databases were isolated from your local data.');
} catch (error) {
  console.error(logs.slice(-10000));
  throw error;
} finally {
  if (server.pid && server.exitCode === null) {
    if (process.platform === 'win32') spawnSync('taskkill', ['/pid', String(server.pid), '/T', '/F'], { windowsHide: true });
    else server.kill('SIGTERM');
    if (server.exitCode === null) await new Promise(resolve => server.once('exit', resolve));
  }
  await rm(directory, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 });
}
