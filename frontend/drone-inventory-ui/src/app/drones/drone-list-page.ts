import { Component, inject, signal } from '@angular/core';
import { Drone } from '../api/api-models';
import { DroneApi } from './drone-api';

// Lists all drones with name, status and description (REQ-002).
// The status comes from the backend, where it is derived from the active loan.
@Component({
  selector: 'app-drone-list-page',
  templateUrl: './drone-list-page.html',
  styleUrl: './drone-list-page.css'
})
export class DroneListPage {
  private readonly droneApi = inject(DroneApi);

  protected readonly drones = signal<Drone[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.droneApi.list().subscribe({
      next: (drones) => {
        this.drones.set(drones);
        this.error.set(null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The drones could not be loaded.');
        this.loading.set(false);
      }
    });
  }
}
