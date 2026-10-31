import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Register } from './register';

describe('Register Component', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create and have invalid form initially', () => {
    expect(component).toBeTruthy();
    expect(component.form.valid).toBe(false);
  });

  it('should detect password mismatch', () => {
    component.form.controls.username.setValue('validuser');
    component.form.controls.password.setValue('password123');
    component.form.controls.confirmPassword.setValue('different123');

    expect(component.form.valid).toBe(false);
    expect(component.form.errors?.['passwordsMismatch']).toBe(true);

    component.form.controls.confirmPassword.setValue('password123');
    expect(component.form.valid).toBe(true);
  });
});
