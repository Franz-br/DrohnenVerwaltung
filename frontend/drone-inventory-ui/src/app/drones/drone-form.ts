import { Component, inject, output, signal } from '@angular/core';
import { form, FormField, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { Drone } from '../api/api-models';
import { problemMessage } from '../api/problem-message';
import { DroneApi } from './drone-api';

// Creates a drone with name and description (REQ-001). The status is never entered:
// it is derived by the backend. Name rules (not empty, unique) are enforced by the backend;
// the required check here only gives earlier feedback.
@Component({
  selector: 'app-drone-form',
  imports: [FormField],
  templateUrl: './drone-form.html',
  styleUrl: './drone-form.css'
})
export class DroneForm {
  private readonly droneApi = inject(DroneApi);

  readonly created = output<Drone>();

  private readonly model = signal({ name: '', description: '' });
  protected readonly droneForm = form(this.model, (path) => {
    required(path.name, { message: 'Please enter a name.' });
  });
  protected readonly serverError = signal<string | null>(null);

  protected save(event: Event): void {
    event.preventDefault();
    submit(this.droneForm, async () => {
      try {
        const drone = await firstValueFrom(this.droneApi.create(this.model()));
        this.serverError.set(null);
        this.droneForm().reset({ name: '', description: '' });
        this.created.emit(drone);
      } catch (error) {
        this.serverError.set(problemMessage(error));
      }
      return undefined;
    });
  }
}
