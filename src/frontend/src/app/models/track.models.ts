export const trackStatuses = ['draft', 'submitted', 'distributed'] as const;
export type TrackStatus = typeof trackStatuses[number];

export function parseStatus(value: string | null): TrackStatus | null {
  return trackStatuses.find(status => status === value?.toLowerCase()) ?? null;
}

export interface TrackSummary {
  id: number;
  title: string;
  artistName: string;
  genre: string;
  status: TrackStatus;
}

export interface DspAssignment {
  dspId: number;
  dspName: string;
  submittedAt: string;
  status: 'pending' | 'live' | 'rejected';
}

export interface TrackDetails extends TrackSummary {
  artistId: number;
  isrc: string;
  releaseDate: string;
  assignments: DspAssignment[];
}
