import { TestBed } from '@angular/core/testing';
import { SettingsService, DEFAULT_WORD_LENGTH } from './settings.service';

describe('SettingsService', () => {
  let service: SettingsService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(SettingsService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should initialize with default word length of 5', () => {
    expect(service.wordLength()).toBe(DEFAULT_WORD_LENGTH);
  });

  it('should update word length when valid length is provided and persist to localStorage', () => {
    service.setWordLength(6);
    expect(service.wordLength()).toBe(6);
    expect(localStorage.getItem('wordle_word_length')).toBe('6');

    service.setWordLength(3);
    expect(service.wordLength()).toBe(3);

    service.setWordLength(8);
    expect(service.wordLength()).toBe(8);
  });

  it('should ignore invalid word lengths', () => {
    service.setWordLength(6);
    service.setWordLength(2); // Invalid (< 3)
    expect(service.wordLength()).toBe(6);

    service.setWordLength(9); // Invalid (> 8)
    expect(service.wordLength()).toBe(6);
  });

  it('should toggle settings modal state', () => {
    expect(service.isSettingsModalOpen()).toBe(false);
    service.openSettings();
    expect(service.isSettingsModalOpen()).toBe(true);
    service.closeSettings();
    expect(service.isSettingsModalOpen()).toBe(false);
    service.toggleSettings();
    expect(service.isSettingsModalOpen()).toBe(true);
  });
});
