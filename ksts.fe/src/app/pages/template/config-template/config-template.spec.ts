import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfigTemplate } from './config-template';

describe('ConfigTemplate', () => {
  let component: ConfigTemplate;
  let fixture: ComponentFixture<ConfigTemplate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfigTemplate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConfigTemplate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
