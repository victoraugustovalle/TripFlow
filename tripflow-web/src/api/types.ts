// Espelha os DTOs do backend (TripFlow.Application/*/DTOs) - mantido a mao, sem geracao
// automatica, entao qualquer mudanca no shape do backend precisa ser refletida aqui tambem.

export type GlobalRole = 0 | 1; // User | Admin
export type TripRole = 0 | 1 | 2; // Viewer | Editor | Owner
export type TripStatus = 0 | 1 | 2 | 3; // Planning | Ongoing | Completed | Cancelled
export type ParticipantStatus = 0 | 1 | 2; // Invited | Accepted | Declined

export interface UserDto {
  id: string;
  name: string;
  email: string;
  globalRole: GlobalRole;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
}

export interface AccessTokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: UserDto;
}

export interface LoginResponse {
  requiresTwoFactor: boolean;
  twoFactorChallengeToken: string | null;
  auth: AccessTokenResponse | null;
}

export interface TripDto {
  id: string;
  name: string;
  destination: string | null;
  description: string | null;
  startDate: string | null;
  endDate: string | null;
  currency: string;
  status: TripStatus;
  createdByUserId: string;
  createdAt: string;
}

export interface TripSummaryDto {
  id: string;
  name: string;
  destination: string | null;
  status: TripStatus;
  startDate: string | null;
  endDate: string | null;
  myRole: TripRole;
}

export interface ParticipantDto {
  id: string;
  tripId: string;
  userId: string | null;
  invitedEmail: string;
  displayName: string | null;
  role: TripRole;
  status: ParticipantStatus;
  createdAt: string;
}

export interface ExpenseSplitDto {
  participantId: string;
  shareAmount: number;
}

export interface ExpenseDto {
  id: string;
  description: string;
  amount: number;
  category: string;
  paidByParticipantId: string;
  expenseDate: string;
  createdAt: string;
  splits: ExpenseSplitDto[];
}

export interface ParticipantBalanceDto {
  participantId: string;
  totalPaid: number;
  totalOwed: number;
  net: number;
}

export interface SettlementTransferDto {
  fromParticipantId: string;
  toParticipantId: string;
  amount: number;
}

export interface SettlementDto {
  balances: ParticipantBalanceDto[];
  transfers: SettlementTransferDto[];
}

export interface ChecklistItemDto {
  id: string;
  title: string;
  isDone: boolean;
  assignedToParticipantId: string | null;
  dueDate: string | null;
  createdAt: string;
}
