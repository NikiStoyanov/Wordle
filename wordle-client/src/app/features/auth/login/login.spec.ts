import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Login } from './login';

describe('Login Component', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create and have invalid form initially', () => {
    expect(component).toBeTruthy();
    expect(component.form.valid).toBe(false);
  });

  it('should validate username and password requirements', () => {
    component.form.controls.username.setValue('ab');
    component.form.controls.password.setValue('123');
    expect(component.form.valid).toBe(false);

    component.form.controls.username.setValue('validuser');
    component.form.controls.password.setValue('validpass');
    expect(component.form.valid).toBe(true);
  });
});
