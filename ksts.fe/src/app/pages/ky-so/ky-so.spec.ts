import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KySo } from './ky-so';

describe('KySo', () => {
  let component: KySo;
  let fixture: ComponentFixture<KySo>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KySo]
    })
    .compileComponents();

    fixture = TestBed.createComponent(KySo);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
