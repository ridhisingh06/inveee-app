/**
 * Shared status normalisation utility.
 * Converts any backend enum string (PendingWithIssuer, ISSUED, etc.)
 * to a consistent lowercase key used across all component templates.
 */

export function normalizeStatus(status: string | null | undefined): string {
  const s = (status ?? '').toLowerCase().trim();
  // Legacy aliases returned by older backend records
  if (s === 'requested') return 'pendingwithissuer';
  if (s === 'issued')    return 'pendingadminapproval';
  return s;
}

export function getStatusLabel(status: string | null | undefined): string {
  const s = normalizeStatus(status);
  switch (s) {
    case 'pendingwithissuer':    return 'Pending with Issuer';
    case 'pendingadminapproval': return 'Pending Admin Approval';
    case 'notissued':            return 'Not Issued';
    case 'approved':             return 'Approved';
    case 'rejected':             return 'Rejected';
    case 'received':             return 'Received';
    default:                     return status ?? 'Pending';
  }
}

export function getStatusClass(status: string | null | undefined): string {
  const s = normalizeStatus(status);
  switch (s) {
    case 'pendingwithissuer':    return 'badge requested';
    case 'pendingadminapproval': return 'badge issued';
    case 'notissued':            return 'badge not-issued';
    case 'approved':             return 'badge approved';
    case 'rejected':             return 'badge rejected';
    case 'received':             return 'badge received';
    default:                     return 'badge';
  }
}
