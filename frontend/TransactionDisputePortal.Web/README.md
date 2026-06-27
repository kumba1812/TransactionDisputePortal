# Capitec Transaction Dispute Portal — Frontend

React 19 + Vite frontend for the Transaction Dispute Portal.

## Stack

| | |
|---|---|
| Framework | React 19.2.6 |
| Build tool | Vite 8.0.12 |
| HTTP client | Axios 1.7.2 (Bearer injection + 401 interceptor) |
| Auth | JWT stored in `sessionStorage`; 5-min inactivity timeout |
| Tests | Vitest 4.1.9 + React Testing Library — **41 tests, 0 failures** |

## Development

```powershell
npm install
npm run dev       # http://localhost:5173
```

Requires the backend API running at `http://localhost:5115`.

## Tests

```powershell
npm run test:run   # single run
npm run test       # watch mode
```

## Production Build

```powershell
npm run build      # outputs to dist/
```

## Seeded Login Credentials

| Username | Password | Role |
|---|---|---|
| admin | Admin123! | Admin |
| banker | Banker123! | Banker |
| client | Client123! | Client |
| readonly | Readonly123! | ReadOnly |