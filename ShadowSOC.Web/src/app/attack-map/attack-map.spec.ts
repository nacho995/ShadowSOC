import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AttackMap } from './attack-map';

describe('AttackMap', () => {
  let component: AttackMap;
  let fixture: ComponentFixture<AttackMap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AttackMap],
    }).compileComponents();

    fixture = TestBed.createComponent(AttackMap);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
