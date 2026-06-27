import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { DisputeHistory } from '../DisputeHistory'
import { mockUsers, mockDisputes } from '../../test/fixtures'

vi.mock('../../context/AuthContext', () => ({ useAuth: vi.fn() }))
vi.mock('../../services/api', () => ({
  disputeApi: {
    getDisputes: vi.fn(),
    acquireLock: vi.fn(),
    releaseLock: vi.fn(),
    updateDispute: vi.fn(),
  },
}))
vi.mock('../DisputeStatusModal', () => ({
  DisputeStatusModal: ({ dispute, onClose }) => (
    <div data-testid="status-modal">
      Modal for dispute {dispute.id}
      <button onClick={onClose}>Close Modal</button>
    </div>
  ),
}))
vi.mock('../../styles/DisputeHistory.css', () => ({}))
vi.mock('../../utils/statusHelpers', () => ({
  getStatusLabel: () => 'Pending',
  formatCurrency: (n) => `R ${n}`,
  formatDate: (d) => d,
}))

import { useAuth } from '../../context/AuthContext'
import { disputeApi } from '../../services/api'

const unlockedDispute = {
  ...mockDisputes[0],
  isLocked: false,
  lockedByName: null,
  lockedAt: null,
}

describe('DisputeHistory', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    disputeApi.getDisputes.mockResolvedValue({ data: [unlockedDispute] })
  })

  it('renders dispute cards from API data', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.getByText(/fraud/i)).toBeInTheDocument()
    })
  })

  it('"Update Status" button is visible for Banker', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update status/i })).toBeInTheDocument()
    })
  })

  it('"Update Status" button is visible for Admin', async () => {
    useAuth.mockReturnValue({ user: mockUsers.admin })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /update status/i })).toBeInTheDocument()
    })
  })

  it('"Update Status" button is hidden for Client', async () => {
    useAuth.mockReturnValue({ user: mockUsers.client })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /update status/i })).toBeNull()
    })
  })

  it('"Update Status" button is hidden for ReadOnly', async () => {
    useAuth.mockReturnValue({ user: mockUsers.readonly })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /update status/i })).toBeNull()
    })
  })

  it('opens modal when acquireLock succeeds (200)', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker })
    disputeApi.acquireLock.mockResolvedValueOnce({ data: unlockedDispute })
    render(<DisputeHistory />)

    await waitFor(() => screen.getByRole('button', { name: /update status/i }))
    fireEvent.click(screen.getByRole('button', { name: /update status/i }))

    await waitFor(() => {
      expect(screen.getByTestId('status-modal')).toBeInTheDocument()
    })
  })

  it('shows inline warning when acquireLock returns 409', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker })
    disputeApi.acquireLock.mockRejectedValueOnce({
      response: { status: 409, data: { lockedByName: 'Other Banker' } },
    })
    render(<DisputeHistory />)

    await waitFor(() => screen.getByRole('button', { name: /update status/i }))
    fireEvent.click(screen.getByRole('button', { name: /update status/i }))

    await waitFor(() => {
      expect(screen.getByText(/currently being reviewed by Other Banker/i)).toBeInTheDocument()
    })
  })

  it('shows lock badge when dispute is locked by someone else', async () => {
    useAuth.mockReturnValue({ user: mockUsers.banker }) // Banker One (id=2)
    const lockedByOther = {
      ...unlockedDispute,
      isLocked: true,
      lockedByName: 'Banker Two',
      lockedAt: new Date(Date.now() - 2 * 60 * 1000).toISOString(),
    }
    disputeApi.getDisputes.mockResolvedValueOnce({ data: [lockedByOther] })
    render(<DisputeHistory />)

    await waitFor(() => {
      expect(screen.getByText(/banker two/i)).toBeInTheDocument()
    })
  })
})
