import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { LoginPage } from '../LoginPage'

// Mock AuthContext — vi.mock is hoisted to top of file by Vitest
vi.mock('../../context/AuthContext', () => ({
  useAuth: vi.fn(),
}))

// Mock CSS import
vi.mock('../../styles/LoginPage.css', () => ({}))

import { useAuth } from '../../context/AuthContext'

describe('LoginPage', () => {
  const mockLogin = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    useAuth.mockReturnValue({ login: mockLogin })
  })

  it('renders username and password inputs', () => {
    render(<LoginPage />)
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
  })

  it('renders Sign In button', () => {
    render(<LoginPage />)
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('calls login() with trimmed username and password on submit', async () => {
    mockLogin.mockResolvedValueOnce(undefined)
    render(<LoginPage />)

    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: '  admin  ' } })
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'Admin123!' } })
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith('admin', 'Admin123!')
    })
  })

  it('shows error message when login is rejected', async () => {
    mockLogin.mockRejectedValueOnce(new Error('Invalid credentials'))
    render(<LoginPage />)

    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'bad' } })
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrong' } })
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
    })
  })

  it('shows validation error when fields are empty', async () => {
    render(<LoginPage />)
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByText(/username and password are required/i)).toBeInTheDocument()
    })
  })

  it('disables the button while loading', async () => {
    // login takes time
    mockLogin.mockReturnValueOnce(new Promise(() => {}))
    render(<LoginPage />)

    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'u' } })
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'p' } })
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled()
    })
  })
})
