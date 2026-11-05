import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, min, required, submit } from '@angular/forms/signals';
import { firstValueFrom, forkJoin } from 'rxjs';
import { Drone, StudentGroup } from '../api/api-models';
import { problemMessage } from '../api/problem-message';
import { DroneApi } from '../drones/drone-api';
import { StudentGroupApi } from '../student-groups/student-group-api';
import { LoanApi } from './loan-api';

interface LoanFormModel {
  droneId: string;
  studentGroupId: string;
  batteries: number;
  propellers: number;
}

const emptyLoan: LoanFormModel = { droneId: '', studentGroupId: '', batteries: 0, propellers: 0 };

// Loans an available drone with batteries and propellers to a student group
// (REQ-004 to REQ-006). The loan rules are enforced by the backend: only available drones are
// offered here for convenience, and a rejected loan (e.g. 409) shows the backend's message.
@Component({
  selector: 'app-loan-form-page',
  imports: [FormField],
  templateUrl: './loan-form-page.html',
  styleUrl: './loan-form-page.css'
})
export class LoanFormPage {
  private readonly droneApi = inject(DroneApi);
  private readonly studentGroupApi = inject(StudentGroupApi);
  private readonly loanApi = inject(LoanApi);

  private readonly drones = signal<Drone[]>([]);
  protected readonly availableDrones = computed(() =>
    this.drones().filter((drone) => drone.status === 'Available')
  );
  protected readonly groups = signal<StudentGroup[]>([]);
  protected readonly loadError = signal<string | null>(null);

  private readonly model = signal<LoanFormModel>({ ...emptyLoan });
  protected readonly loanForm = form(this.model, (path) => {
    required(path.droneId, { message: 'Please choose a drone.' });
    required(path.studentGroupId, { message: 'Please choose a student group.' });
    min(path.batteries, 0, { message: 'Batteries must not be negative.' });
    min(path.propellers, 0, { message: 'Propellers must not be negative.' });
  });
  protected readonly serverError = signal<string | null>(null);
  protected readonly confirmation = signal<string | null>(null);

  constructor() {
    this.loadOptions();
  }

  private loadOptions(): void {
    forkJoin({ drones: this.droneApi.list(), groups: this.studentGroupApi.list() }).subscribe({
      next: ({ drones, groups }) => {
        this.drones.set(drones);
        this.groups.set(groups);
        this.loadError.set(null);
      },
      error: () => this.loadError.set('The drones and student groups could not be loaded.')
    });
  }

  protected save(event: Event): void {
    event.preventDefault();
    submit(this.loanForm, async () => {
      const value = this.model();
      try {
        const loan = await firstValueFrom(
          this.loanApi.create({
            droneId: Number(value.droneId),
            studentGroupId: Number(value.studentGroupId),
            items: [
              { type: 'Battery', quantity: value.batteries },
              { type: 'Propeller', quantity: value.propellers }
            ]
          })
        );
        this.serverError.set(null);
        this.confirmation.set(`${loan.droneName} was loaned to ${loan.studentGroupName}.`);
        this.loanForm().reset({ ...emptyLoan });
        // The loaned drone is no longer available, so the drone options are reloaded.
        this.loadOptions();
      } catch (error) {
        this.confirmation.set(null);
        this.serverError.set(problemMessage(error));
      }
      return undefined;
    });
  }
}
