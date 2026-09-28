/** Full profile of the currently authenticated user. */
export interface User {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

/** Public information about another user, returned by the friend search endpoint. */
export interface UserPublic {
  id: string;
  username: string;
  wishlistId: string;
}

/** Minimal info about a friend, used to populate pickers (gift visibility, "chi l'ha regalato"). */
export interface UserSummary {
  id: string;
  username: string;
}
