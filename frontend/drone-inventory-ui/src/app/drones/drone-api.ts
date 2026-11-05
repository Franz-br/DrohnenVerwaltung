import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateDroneRequest, Drone } from '../api/api-models';

@Service()
export class DroneApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Drone[]> {
    return this.http.get<Drone[]>('/api/drones');
  }

  create(request: CreateDroneRequest): Observable<Drone> {
    return this.http.post<Drone>('/api/drones', request);
  }
}
