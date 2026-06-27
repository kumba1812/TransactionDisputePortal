import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import App from '../../App'

vi.mock('../../context/AuthContext', () => ({
  useAuth: vi.fn(),
}))

// Stub child components to prevent their API calls
vi.mock('../../components/LoginPage', () => ({
  LoginPage: () => <div data-testid="login-page">LoginPage</div>,
}))
vi.mock('../../components/TransactionsList', () => ({
  TransactionsList: () => <div data-testid="transactions-list">Transactions</div>,
}))
vi.mock('../../components/DisputeHistory', () => ({
  DisputeHistory: () => <div data-testid="dispute-history">History</div>,
}))
vi.mock('../../components/DisputeForm', () => ({
  DisputeForm: () => <div data-testid="dispute-form">Form</div>,
}))

vi.mock('../../App.css', () => ({}))

import { useAuth } from '../../context/AuthContext'
import { mockUsers } from '../../test/fixtures'

describe('App', () => {
  beforeEach(() => vi.clearAllMocks())

  it('renders LoginPage when not authenticated', () => {
    useAuth.mockReturnValue({ isAuthenticated: false, user: null, logout: vi.fn() })
    render(<App />)
    expect(screen.getByTestId('login-page')).toBeInTheDocument()
  })

  it('renders main layout when authenticated', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      user: mockUsers.client,
      logout: vi.fn(),
    })
    render(<App />)
    expect(screen.getByTestId('transactions-list')).toBeInTheDocument()
  })

  it('shows the logged-in user full name in header', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      user: mockUsers.banker,
      logout: vi.fn(),
    })
    render(<App />)
    expect(screen.getByText('Banker One')).toBeInTheDocument()
  })

  it('shows the role badge', () => {
    useAuth.mockReturnValue({
      isAuthenticated: true,
      user: mockUsers.admin,
      logout: vi.fn(),
    })
    render(<App />)
    expect(screen.getByText('Admin')).toBeInTheDocument()
  })

  it('calls logout() when Sign Out button is clicked', () => {
    const mockLogout = vi.fn()
    useAuth.mockReturnValue({
      isAuthenticated: true,
      user: mockUsers.client,
      logout: mockLogout,
    })
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: /sign out/i }))
    expect(mockLogout).toHaveBeenCalledOnce()
  })
})
