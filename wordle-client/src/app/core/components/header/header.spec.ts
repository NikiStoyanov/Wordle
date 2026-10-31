import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Header } from './header';
import { SettingsService } from '../../services/settings.service';

describe('Header', () => {
  let component: Header;
  let fixture: ComponentFixture<Header>;
  let settingsService: SettingsService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    settingsService = TestBed.inject(SettingsService);
    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle and close settings modal', () => {
    expect(component.isSettingsOpen()).toBe(false);
    component.toggleSettings();
    expect(component.isSettingsOpen()).toBe(true);
    component.closeSettings();
    expect(component.isSettingsOpen()).toBe(false);
  });

  it('should change word length via selectWordLength', () => {
    component.selectWordLength(6);
    expect(settingsService.wordLength()).toBe(6);
    expect(component.currentWordLength()).toBe(6);
  });
});
