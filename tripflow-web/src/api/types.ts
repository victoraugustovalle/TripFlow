// Espelha os DTOs do backend (TripFlow.Application/*/DTOs) - mantido a mao, sem geracao
// automatica, entao qualquer mudanca no shape do backend precisa ser refletida aqui tambem.

export type GlobalRole = 0 | 1; // User | Admin
export type TripRole = 0 | 1 | 2; // Viewer | Editor | Owner
export type TripStatus = 0 | 1 | 2 | 3; // Planning | Ongoing | Completed | Cancelled
export type ParticipantStatus = 0 | 1 | 2; // Invited | Accepted | Declined
export type ReservationType = 0 | 1 | 2 | 3; // Flight | Hotel | CarRental | Other
export type DocumentCategory = 0 | 1 | 2 | 3 | 4; // Passport | Ticket | Voucher | Insurance | Other

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
  coverImageUrl: string | null;
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
  coverImageUrl: string | null;
  myRole: TripRole;
}

export interface JournalEntryDto {
  tripId: string;
  name: string;
  destination: string | null;
  startDate: string | null;
  endDate: string | null;
  coverImageUrl: string | null;
  currency: string;
  totalSpent: number;
  averageRating: number | null;
}

export interface MyJournalDto {
  tripsCompletedCount: number;
  destinationsCount: number;
  totalDaysTraveled: number;
  trips: JournalEntryDto[];
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

export type SettlementRecordStatus = 0 | 1; // Pending | Confirmed

export interface SettlementRecordDto {
  id: string;
  fromParticipantId: string;
  toParticipantId: string;
  amount: number;
  status: SettlementRecordStatus;
  createdAt: string;
  confirmedAt: string | null;
}

export interface SettlementDto {
  balances: ParticipantBalanceDto[];
  transfers: SettlementTransferDto[];
  pendingSettlements: SettlementRecordDto[];
}

export interface ChecklistItemDto {
  id: string;
  title: string;
  isDone: boolean;
  assignedToParticipantId: string | null;
  dueDate: string | null;
  createdAt: string;
}

export type ItineraryItemType = 0 | 1 | 2 | 3 | 4; // Activity | Transport | Lodging | Meal | Other
export type ItineraryItemStatus = 0 | 1; // Confirmed | Proposed

export interface ItineraryProposalOptionDto {
  id: string;
  title: string;
  description: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
  voteCount: number;
}

export interface ItineraryItemDto {
  id: string;
  title: string;
  description: string | null;
  type: ItineraryItemType;
  itemDate: string;
  startTime: string | null;
  endTime: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
  createdAt: string;
  status: ItineraryItemStatus;
  proposalOptions: ItineraryProposalOptionDto[];
  myVotedOptionId: string | null;
}

export interface ItineraryDayWeatherDto {
  date: string;
  temperatureMaxC: number | null;
  temperatureMinC: number | null;
  precipitationProbabilityPercent: number | null;
  suggestedChecklistItems: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GeocodeResultDto {
  displayName: string;
  latitude: number;
  longitude: number;
}

export interface BudgetCategoryOverviewDto {
  category: string;
  plannedAmount: number;
  actualAmount: number;
}

export interface BudgetOverviewDto {
  totalPlanned: number;
  totalActual: number;
  categories: BudgetCategoryOverviewDto[];
}

export interface ChecklistOverviewDto {
  totalCount: number;
  doneCount: number;
}

export interface SettlementOverviewDto {
  myNetAmount: number;
  myTransfers: SettlementTransferDto[];
  myPendingSettlements: SettlementRecordDto[];
}

export interface UpcomingItineraryItemDto {
  id: string;
  title: string;
  type: ItineraryItemType;
  itemDate: string;
  startTime: string | null;
  location: string | null;
}

// Corresponde a um dos valores de Tab em TripDetailPage - "itinerary" | "expenses" |
// "checklist" | "participants" | "documents"
export interface TripReadinessItemDto {
  key: string;
  label: string;
  targetTab: string;
}

export interface TripReadinessDto {
  percentReady: number;
  pendingItems: TripReadinessItemDto[];
}

export interface TripOverviewDto {
  pendingInvitesCount: number;
  budget: BudgetOverviewDto;
  checklist: ChecklistOverviewDto;
  settlement: SettlementOverviewDto;
  upcomingItineraryItems: UpcomingItineraryItemDto[];
  readiness: TripReadinessDto;
}

export interface BudgetDto {
  id: string;
  category: string;
  plannedAmount: number;
  actualAmount: number;
}

export interface ReservationDto {
  id: string;
  type: ReservationType;
  title: string;
  providerName: string | null;
  confirmationCode: string | null;
  startAt: string | null;
  endAt: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
  price: number | null;
  currency: string | null;
  notes: string | null;
  itineraryItemId: string | null;
  createdAt: string;
}

export interface TopSpenderDto {
  participantId: string;
  displayName: string;
  totalPaid: number;
}

export interface TripMemoryDto {
  participantId: string;
  displayName: string;
  highlight: string | null;
  rating: number | null;
  photoUrl: string | null;
  updatedAt: string;
}

export interface TripRetrospectiveDto {
  tripName: string;
  startDate: string | null;
  endDate: string | null;
  durationDays: number | null;
  totalSpent: number;
  totalPlanned: number;
  topSpenders: TopSpenderDto[];
  checklistTotalCount: number;
  checklistDoneCount: number;
  itineraryItemsCount: number;
  participantsCount: number;
  memories: TripMemoryDto[];
  averageRating: number | null;
}

// ParticipantJoined | ChecklistItemAssigned | ExpenseCreated | BudgetExceeded |
// ReservationCreated | ReservationUpdated | ItineraryItemUpdated | DocumentDeleted |
// SettlementMarkedPaid | SettlementConfirmed | TripStartingSoonNotReady | BudgetPaceExceeding
export type NotificationType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11;

export interface NotificationDto {
  id: string;
  tripId: string;
  tripName: string;
  type: NotificationType;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationsPageDto {
  items: NotificationDto[];
  unreadCount: number;
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PresenceUserDto {
  userId: string;
  displayName: string;
}

export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  category: DocumentCategory;
  uploadedByParticipantId: string;
  createdAt: string;
}

// ExpenseCreated | ExpenseDeleted | ChecklistItemCreated | ChecklistItemAssigned |
// ChecklistItemDeleted | ItineraryItemCreated | ItineraryItemDeleted | ReservationCreated |
// ReservationDeleted | ParticipantJoined
export type ActivityType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;

export interface ActivityLogEntryDto {
  id: string;
  type: ActivityType;
  entityType: string;
  entityId: string | null;
  message: string;
  actorDisplayName: string | null;
  actorUserId: string | null;
  createdAt: string;
}
