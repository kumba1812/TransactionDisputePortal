import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DisputeStatusModal } from '../DisputeStatusModal'

vi.mock('../../services/api', () => ({
  disputeApi: {
    releaseLock: vi.fn(),
    updateDispute: vi.fn(),
  },
}))
vi.mock('../../styles/DisputeStatusModal.css', () => ({}))
vi.mock('../../utils/statusHelpers', () => ({
  getStatusLabel: () => 'Pending',
  formatDate: (d) => d,
}))

import { disputeApi } from '../../services/api'

const activeDispute = {
  id: 1,
  transactionId: 1,
  customerId: 4,
  reason: 'Fraud',
  description: 'I did not make this',
  status: 0,
  resolutionNotes: null,
  refundAmount: 100,
  lockedAt: new Date(Date.now() - 2 * 60 * 1000).toISOString(), // 2 min ago — active
}

const expiredDispute = {
  ...activeDispute,
  lockedAt: new Date(Date.now() - 15 * 60 * 1000).toISOString(), // 15 min ago — expired
}

describe('DisputeStatusModal', () => {
  beforeEach(() => vi.clearAllMocks())

  it('renders the countdown timer for an active lock', () => {
    render(
      <DisputeStatusModal
        dispute={activeDispute}
        onClose={vi.fn()}
        onStatusUpdated={vi.fn()}
      />
    )
    // Timer should show 07:xx (approx 8 min remaining)
    expect(screen.getByText(/\d{2}:\d{2}/)).toBeInTheDocument()
  })

  it('calls releaseLock on close', async () => {
    const user = userEvent.setup()
    disputeApi.releaseLock.mockResolvedValueOnce({})
    const onClose = vi.fn()
    render(
      <DisputeStatusModal
        dispute={activeDispute}
        onClose={onClose}
        onStatusUpdated={vi.fn()}
      />
    )

    await user.click(screen.getByRole('button', { name: /cancel/i }))

    await waitFor(() => {
      expect(disputeApi.releaseLock).toHaveBeenCalledWith(activeDispute.id)
      expect(onClose).toHaveBeenCalled()
    })
  })

  it('calls updateDispute on submit with changed status', async () => {
    const user = userEvent.setup()
    disputeApi.updateDispute.mockResolvedValueOnce({ data: {} })
    const onStatusUpdated = vi.fn()
    render(
      <DisputeStatusModal
        dispute={activeDispute}
        onClose={vi.fn()}
        onStatusUpdated={onStatusUpdated}
      />
    )

    await user.selectOptions(screen.getByRole('combobox'), '2') // Resolved
    await user.click(screen.getByRole('button', { name: /update status/i }))

    await waitFor(() => {
      expect(disputeApi.updateDispute).toHaveBeenCalledWith(
        activeDispute.id,
        expect.objectContaining({ status: 2 })
      )
      expect(onStatusUpdated).toHaveBeenCalled()
    })
  })

  it('shows lock-expired message and submit is disabled when lock has expired', () => {
    render(
      <DisputeStatusModal
        dispute={expiredDispute}
        onClose={vi.fn()}
        onStatusUpdated={vi.fn()}
      />
    )

    expect(screen.getByText(/lock expired/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /update status/i })).toBeDisabled()
    expect(disputeApi.updateDispute).not.toHaveBeenCalled()
  })

  it('shows API error message on update failure', async () => {
    const user = userEvent.setup()
    disputeApi.updateDispute.mockRejectedValueOnce({
      response: { data: { message: 'You do not hold the lock' } },
    })
    render(
      <DisputeStatusModal
        dispute={activeDispute}
        onClose={vi.fn()}
        onStatusUpdated={vi.fn()}
      />
    )

    await user.selectOptions(screen.getByRole('combobox'), '2')
    await user.click(screen.getByRole('button', { name: /update status/i }))

    await waitFor(() => {
      expect(screen.getByText(/you do not hold the lock/i)).toBeInTheDocument()
    })
  })
})
