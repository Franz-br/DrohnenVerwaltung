import { Routes } from '@angular/router';

// Every feature page is lazy loaded (design D9).
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'drones' },
  {
    path: 'drones',
    title: 'Drones',
    loadComponent: () => import('./drones/drone-list-page').then((m) => m.DroneListPage)
  },
  {
    path: 'student-groups',
    title: 'Student groups',
    loadComponent: () =>
      import('./student-groups/student-groups-page').then((m) => m.StudentGroupsPage)
  },
  {
    path: 'loans/new',
    title: 'Loan a drone',
    loadComponent: () => import('./loans/loan-form-page').then((m) => m.LoanFormPage)
  },
  {
    path: 'loans/history',
    title: 'Loan history',
    loadComponent: () => import('./loans/loan-history-page').then((m) => m.LoanHistoryPage)
  },
  {
    path: 'loans',
    title: 'Active loans',
    loadComponent: () => import('./loans/active-loans-page').then((m) => m.ActiveLoansPage)
  },
  { path: '**', redirectTo: 'drones' }
];
