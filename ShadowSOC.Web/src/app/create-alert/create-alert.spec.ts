import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateOrder } from './create-alert';

describe('CreateOrder', () => {
  let component: CreateOrder;
  let fixture: ComponentFixture<CreateOrder>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateOrder],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateOrder);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
