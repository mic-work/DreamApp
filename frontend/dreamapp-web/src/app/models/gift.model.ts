export type GiftPriority = 'Low' | 'Medium' | 'High';

export interface Gift {
  id: string;
  wishlistId: string;
  name: string;
  description?: string | null;
  notes?: string | null;
  /** @deprecated use `photos` instead — kept only for gifts saved before multi-photo support. */
  imageUrl?: string | null;
  /** Photo gallery, in display order. */
  photos: string[];
  productUrl?: string | null;
  priority: GiftPriority;
  createdAt: string;
  updatedAt: string;
  isOwner: boolean;
  /** Only meaningful when isOwner is true: friends this gift is restricted to. Empty = visible to every friend. */
  visibleToUserIds: string[];
  /**
   * Whether this gift is definitively confirmed as received/given. When
   * isOwner is true: the owner recorded "I already received this, from X"
   * (immediate — see receivedFromUserId/Username). When isOwner is false:
   * the OWNER has named the current user as the giver — the user's own
   * claim alone is never enough on its own, see myClaimPending below.
   */
  receivedByMe: boolean;
  /** Only meaningful when isOwner is true — who the owner says gave this gift. */
  receivedFromUserId?: string | null;
  receivedFromUsername?: string | null;
  /** Only meaningful when receivedByMe is true: when it was confirmed — lets a "già regalati"/"ricevuti" list sort by confirmation date. */
  receivedAt?: string | null;
  /**
   * Only meaningful when isOwner is false: the current user has privately
   * claimed "I already gave this", but the owner hasn't confirmed it yet.
   */
  myClaimPending: boolean;
  /**
   * Only meaningful when isOwner is true: ids of friends who have privately
   * claimed "I already gave this" but aren't confirmed yet — a hint for
   * "chi l'ha regalato?", never a confirmation by itself.
   */
  claimedByUserIds: string[];
}

export interface GiftCreateRequest {
  name: string;
  description?: string | null;
  notes?: string | null;
  imageUrls: string[];
  productUrl?: string | null;
  priority: GiftPriority;
  visibleToUserIds: string[];
}

export type GiftUpdateRequest = GiftCreateRequest;

export interface GiftReceptionRequest {
  received: boolean;
  givenByUserId?: string | null;
}
