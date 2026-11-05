import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { Drone } from '../api/api-models';

@Service()
export class DroneApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Drone[]> {
    return this.http.get<Drone[]>('/api/drones');
  }
}
