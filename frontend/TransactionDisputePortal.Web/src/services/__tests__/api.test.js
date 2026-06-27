import { describe, it, expect, vi, beforeEach } from 'vitest'

// Capture interceptors registered by api.js using hoisted refs
const refs = vi.hoisted(() => ({
  requestFn: null,
  responseSuccessFn: null,
  responseErrorFn: null,
}))

vi.mock('axios', () => {
  const mockClient = {
    interceptors: {
      request: {
        use: vi.fn((fn) => { refs.requestFn = fn }),
      },
      response: {
        use: vi.fn((okFn, errFn) => {
          refs.responseSuccessFn = okFn
          refs.responseErrorFn = errFn
        }),
      },
    },
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  }
  return { default: { create: vi.fn(() => mockClient) } }
})

// Importing api.js triggers axios.create() + interceptor registration
import { transactionApi, disputeApi } from '../../services/api'

describe('api client interceptors', () => {
  beforeEach(() => sessionStorage.clear())

  it('attaches Bearer token when token is in sessionStorage', () => {
    sessionStorage.setItem('tdp_token', 'my-token-123')
    const config = { headers: {} }
    const result = refs.requestFn(config)
    expect(result.headers.Authorization).toBe('Bearer my-token-123')
  })

  it('does not attach Authorization header when no token in sessionStorage', () => {
    const config = { headers: {} }
    const result = refs.requestFn(config)
    expect(result.headers.Authorization).toBeUndefined()
  })

  it('clears tdp_token and tdp_user from sessionStorage on 401 response', async () => {
    sessionStorage.setItem('tdp_token', 'tok')
    sessionStorage.setItem('tdp_user', '{"id":1}')

    const error = { response: { status: 401 } }
    await refs.responseErrorFn(error).catch(() => {})

    expect(sessionStorage.getItem('tdp_token')).toBeNull()
    expect(sessionStorage.getItem('tdp_user')).toBeNull()
  })

  it('does not clear sessionStorage for non-401 errors', async () => {
    sessionStorage.setItem('tdp_token', 'tok')
    const error = { response: { status: 500 } }
    await refs.responseErrorFn(error).catch(() => {})

    expect(sessionStorage.getItem('tdp_token')).toBe('tok')
  })
})

describe('api module exports', () => {
  it('transactionApi exposes expected methods', () => {
    expect(typeof transactionApi.getTransactions).toBe('function')
    expect(typeof transactionApi.getTransaction).toBe('function')
    expect(typeof transactionApi.createTransaction).toBe('function')
    expect(typeof transactionApi.updateTransaction).toBe('function')
    expect(typeof transactionApi.deleteTransaction).toBe('function')
  })

  it('disputeApi exposes expected methods', () => {
    expect(typeof disputeApi.getDisputes).toBe('function')
    expect(typeof disputeApi.getDispute).toBe('function')
    expect(typeof disputeApi.createDispute).toBe('function')
    expect(typeof disputeApi.updateDispute).toBe('function')
    expect(typeof disputeApi.deleteDispute).toBe('function')
    expect(typeof disputeApi.acquireLock).toBe('function')
    expect(typeof disputeApi.releaseLock).toBe('function')
  })
})
