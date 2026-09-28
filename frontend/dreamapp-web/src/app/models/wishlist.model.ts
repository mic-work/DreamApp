import { Gift } from './gift.model';

export interface WishlistSummary {
  id: string;
  ownerUsername: string;
  name: string;
  giftsCount: number;
}

export interface WishlistDetail {
  id: string;
  ownerId: string;
  ownerUsername: string;
  name: string;
  createdAt: string;
  isOwner: boolean;
  gifts: Gift[];
}
