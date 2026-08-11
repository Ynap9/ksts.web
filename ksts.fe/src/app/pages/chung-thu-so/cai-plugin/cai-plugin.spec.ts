import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CaiPlugin } from './cai-plugin';

describe('CaiPlugin', () => {
  let component: CaiPlugin;
  let fixture: ComponentFixture<CaiPlugin>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CaiPlugin]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CaiPlugin);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
