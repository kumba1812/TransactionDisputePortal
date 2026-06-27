import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { TransactionsList } from '../TransactionsList'
import { mockUsers, mockTransactions } from '../../test/fixtures'

vi.mock('../../context/AuthContext', () => ({ useAuth: vi.fn() }))
vi.mock('../../services/api', () => ({
  transactionApi: { getTransactions: vi.fn() },
}))
vi.mock('../../styles/TransactionsList.css', () => ({}))
vi.mock('../../utils/statusHelpers', () => ({
  getTransactionStatusLabel: () => 'Completed',
  formatCurrency: (n) => `R ${n}`,
  formatDate: (d) => d,
}))

import { useAuth } from '../../context/AuthContext'
import { transactionApi } from '../../services/api'

describe('TransactionsList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    transactionApi.getTransactions.mockResolvedValue({ data: mockTransactions })
  })

  it('renders transaction rows from API data', async () => {
    useAuth.mockReturnValue({ user: mockUsers.client })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      expect(screen.getByText('Pick n Pay')).toBeInTheDocument()
      expect(screen.getByText('Game')).toBeInTheDocument()
    })
  })

  it('"View/Dispute" button is present for Client', async () => {
    useAuth.mockReturnValue({ user: mockUsers.client })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      const buttons = screen.getAllByRole('button', { name: /view\/dispute/i })
      expect(buttons.length).toBeGreaterThan(0)
    })
  })

  it('"View/Dispute" button is present for Admin', async () => {
    useAuth.mockReturnValue({ user: mockUsers.admin })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      const buttons = screen.getAllByRole('button', { name: /view\/dispute/i })
      expect(buttons.length).toBeGreaterThan(0)
    })
  })

  it('"View/Dispute" button is hidden for Banker', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      expect(screen.queryAllByRole('button', { name: /view\/dispute/i })).toHaveLength(0)
    })
  })

  it('"View/Dispute" button is hidden for ReadOnly', async () => {
    useAuth.mockReturnValue({ user: mockUsers.readonly })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      expect(screen.queryAllByRole('button', { name: /view\/dispute/i })).toHaveLength(0)
    })
  })

  it('shows error message when API call fails', async () => {
    transactionApi.getTransactions.mockRejectedValueOnce(new Error('Network error'))
    useAuth.mockReturnValue({ user: mockUsers.client })
    render(<TransactionsList onSelectTransaction={vi.fn()} />)

    await waitFor(() => {
      expect(screen.getByText(/failed to load transactions/i)).toBeInTheDocument()
    })
  })
})
