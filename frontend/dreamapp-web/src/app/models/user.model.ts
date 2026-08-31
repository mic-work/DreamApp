/** Full profile of the currently authenticated user. */
export interface User {
  id: number;
  username: string;
  email: string;
  createdAt: string;
}

/** Public information about another user, returned by the friend search endpoint. */
export interface UserPublic {
  id: number;
  username: string;
  wishlistId: number;
}
