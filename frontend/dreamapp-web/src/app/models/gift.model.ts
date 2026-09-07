export type GiftPriority = 'Low' | 'Medium' | 'High';

export interface Gift {
  id: number;
  wishlistId: number;
  name: string;
  description?: string | null;
  notes?: string | null;
  imageUrl?: string | null;
  productUrl?: string | null;
  priority: GiftPriority;
  createdAt: string;
  updatedAt: string;
  isOwner: boolean;
}

export interface GiftCreateRequest {
  name: string;
  description?: string | null;
  notes?: string | null;
  imageUrl?: string | null;
  productUrl?: string | null;
  priority: GiftPriority;
}

export type GiftUpdateRequest = GiftCreateRequest;
