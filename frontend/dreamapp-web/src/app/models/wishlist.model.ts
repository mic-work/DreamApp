import { Gift } from './gift.model';

export interface WishlistSummary {
  id: number;
  ownerUsername: string;
  name: string;
  giftsCount: number;
}

export interface WishlistDetail {
  id: number;
  ownerId: number;
  ownerUsername: string;
  name: string;
  createdAt: string;
  isOwner: boolean;
  gifts: Gift[];
}
