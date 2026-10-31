export type LetterState = 'empty' | 'absent' | 'present' | 'correct';

export interface Tile {
  letter: string;
  state: LetterState;
}

export interface Row {
  tiles: Tile[];
}

export interface LetterEvaluation {
  letter: string;
  state: LetterState | number | string;
}

export interface SubmitGuessDto {
  guess: string;
  length?: number;
}

export interface UserStatsDto {
  currentStreak: number;
  maxStreak: number;
  totalGamesPlayed: number;
  totalGamesWon: number;
  winPercentage: number;
  guessDistribution: Record<number, number>;
}

export interface DailyChallengeResultDto {
  isWon: boolean;
  isGameOver: boolean;
  wordLength?: number;
  correctWord?: string | null;
  evaluations: LetterEvaluation[];
  attemptsUsed: number;
  stats?: UserStatsDto | null;
}

export interface PreviousGuessDto {
  guess: string;
  evaluations: LetterEvaluation[];
}

export interface DailyChallengeStatusDto {
  date: string;
  wordLength?: number;
  hasPlayed: boolean;
  isSolved: boolean;
  isGameOver: boolean;
  attemptsUsed: number;
  previousGuesses: PreviousGuessDto[];
  correctWord?: string | null;
  stats: UserStatsDto;
  nextChallengeUtc: string;
}

export function mapServerStateToLetterState(
  state: LetterState | number | string
): LetterState {
  if (state === 2 || state === '2' || String(state).toLowerCase() === 'correct') {
    return 'correct';
  }
  if (state === 1 || state === '1' || String(state).toLowerCase() === 'present') {
    return 'present';
  }
  if (state === 0 || state === '0' || String(state).toLowerCase() === 'absent') {
    return 'absent';
  }
  return 'empty';
}
