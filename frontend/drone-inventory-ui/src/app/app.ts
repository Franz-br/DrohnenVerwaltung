import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly links = [
    { path: '/drones', label: 'Drones' },
    { path: '/student-groups', label: 'Student groups' },
    { path: '/loans/new', label: 'Loan a drone' },
    { path: '/loans', label: 'Active loans' },
    { path: '/loans/history', label: 'Loan history' }
  ];
}
