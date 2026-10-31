import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DailyChallengeResultDto,
  DailyChallengeStatusDto,
  SubmitGuessDto,
  UserStatsDto,
} from '../models/game';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/game`;

  getTodayChallenge(length: number = 5): Observable<DailyChallengeStatusDto> {
    return this.http.get<DailyChallengeStatusDto>(`${this.baseUrl}/today`, {
      params: { length: length.toString() },
    });
  }

  submitGuess(guess: string, length?: number): Observable<DailyChallengeResultDto> {
    const body: SubmitGuessDto = { guess, length: length ?? guess.length };
    return this.http.post<DailyChallengeResultDto>(`${this.baseUrl}/guess`, body);
  }

  getStats(): Observable<UserStatsDto> {
    return this.http.get<UserStatsDto>(`${this.baseUrl}/stats`);
  }

  getAvailableLengths(): Observable<number[]> {
    return this.http.get<number[]>(`${this.baseUrl}/lengths`);
  }
}
