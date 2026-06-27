import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DisputeForm } from '../DisputeForm'
import { mockUsers, mockTransactions } from '../../test/fixtures'

vi.mock('../../context/AuthContext', () => ({ useAuth: vi.fn() }))
vi.mock('../../services/api', () => ({
  disputeApi: {
    getDisputesByTransaction: vi.fn(),
    createDispute: vi.fn(),
  },
}))
vi.mock('../../styles/DisputeForm.css', () => ({}))
vi.mock('../../utils/statusHelpers', () => ({
  formatCurrency: (n) => `R ${n}`,
  formatDate: (d) => d,
}))

import { useAuth } from '../../context/AuthContext'
import { disputeApi } from '../../services/api'

const sampleTx = mockTransactions[0]

describe('DisputeForm', () => {
  const onDisputeCreated = vi.fn()
  const onCancel = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    useAuth.mockReturnValue({ user: mockUsers.client })
    disputeApi.getDisputesByTransaction.mockResolvedValue({ data: [] })
  })

  it('shows "Filing as: [fullName]" for the logged-in user', async () => {
    render(<DisputeForm transaction={sampleTx} onDisputeCreated={onDisputeCreated} onCancel={onCancel} />)

    await waitFor(() => {
      expect(screen.getByText(/filing as:/i)).toBeInTheDocument()
      expect(screen.getByText(/client user/i)).toBeInTheDocument()
    })
  })

  it('calls createDispute on valid submit', async () => {
    const user = userEvent.setup()
    disputeApi.createDispute.mockResolvedValueOnce({ data: { id: 99 } })
    render(<DisputeForm transaction={sampleTx} onDisputeCreated={onDisputeCreated} onCancel={onCancel} />)

    await user.selectOptions(screen.getByRole('combobox'), 'Fraudulent Activity')
    await user.type(screen.getByRole('textbox'), 'This transaction was not authorized by me at all')
    await user.click(screen.getByRole('button', { name: /create dispute/i }))

    await waitFor(() => {
      expect(disputeApi.createDispute).toHaveBeenCalledWith(
        expect.objectContaining({ transactionId: sampleTx.id })
      )
    })
  })

  it('shows validation error when description is too short', async () => {
    const user = userEvent.setup()
    render(<DisputeForm transaction={sampleTx} onDisputeCreated={onDisputeCreated} onCancel={onCancel} />)

    await user.selectOptions(screen.getByRole('combobox'), 'Fraudulent Activity')
    await user.type(screen.getByRole('textbox'), 'short')
    await user.click(screen.getByRole('button', { name: /create dispute/i }))

    await waitFor(() => {
      expect(screen.getByText(/at least 10 characters/i)).toBeInTheDocument()
    })
  })

  it('disables form when an existing dispute is present', async () => {
    disputeApi.getDisputesByTransaction.mockResolvedValueOnce({
      data: [{ id: 1, reason: 'Fraud', status: 0 }],
    })
    render(<DisputeForm transaction={sampleTx} onDisputeCreated={onDisputeCreated} onCancel={onCancel} />)

    await waitFor(() => {
      expect(screen.getByText(/already has an active dispute/i)).toBeInTheDocument()
    })
    // Submit button should be disabled when existing dispute present
    expect(screen.getByRole('button', { name: /create dispute/i })).toBeDisabled()
  })
})
