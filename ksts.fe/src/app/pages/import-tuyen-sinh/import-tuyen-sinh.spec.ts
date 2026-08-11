import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImportTuyenSinh } from './import-tuyen-sinh';

describe('ImportTuyenSinh', () => {
  let component: ImportTuyenSinh;
  let fixture: ComponentFixture<ImportTuyenSinh>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImportTuyenSinh]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ImportTuyenSinh);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
