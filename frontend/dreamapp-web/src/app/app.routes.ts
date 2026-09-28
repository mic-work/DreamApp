import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'home' },

  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/home/home.component').then((m) => m.HomeComponent)
  },
  {
    path: 'wishlist/me',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/my-wishlist/my-wishlist.component').then((m) => m.MyWishlistComponent)
  },
  {
    // Own gift routes must be declared BEFORE 'wishlist/:username/gifts/:id'
    // below: with matching segment counts, Angular's router picks whichever
    // route it finds first, so the literal 'me'/'new'/'edit' segments need
    // priority over the ':username'/':id' wildcards to avoid being swallowed
    // by the generic pattern.
    path: 'wishlist/me/gifts/new',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/add-gift/add-gift.component').then((m) => m.AddGiftComponent)
  },
  {
    path: 'wishlist/me/gifts/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/add-gift/add-gift.component').then((m) => m.AddGiftComponent)
  },
  {
    // Serves both "my own gift" (.../wishlist/me/gifts/4) and "a friend's
    // gift" (.../wishlist/tatina/gifts/7): the :username segment is purely
    // for a readable, contextual URL — GiftDetailComponent fetches by the
    // numeric :id alone and the server re-checks ownership/access itself,
    // so nothing here trusts the username actually matches the gift.
    path: 'wishlist/:username/gifts/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/gift-detail/gift-detail.component').then((m) => m.GiftDetailComponent)
  },
  {
    path: 'wishlist/:username',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/wishlist-detail/wishlist-detail.component').then((m) => m.WishlistDetailComponent)
  },
  {
    path: 'add-friend',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/add-friend/add-friend.component').then((m) => m.AddFriendComponent)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/profile/profile.component').then((m) => m.ProfileComponent)
  },

  { path: '**', redirectTo: 'home' }
];
