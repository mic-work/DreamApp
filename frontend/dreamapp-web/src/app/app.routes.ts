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
    path: 'wishlist/:username',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/wishlist-detail/wishlist-detail.component').then((m) => m.WishlistDetailComponent)
  },
  {
    path: 'gifts/new',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/add-gift/add-gift.component').then((m) => m.AddGiftComponent)
  },
  {
    path: 'gifts/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/add-gift/add-gift.component').then((m) => m.AddGiftComponent)
  },
  {
    path: 'gifts/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/gift-detail/gift-detail.component').then((m) => m.GiftDetailComponent)
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
