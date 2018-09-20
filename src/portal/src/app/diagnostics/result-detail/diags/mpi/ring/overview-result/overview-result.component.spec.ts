import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RingOverviewResultComponent } from './overview-result.component';

fdescribe('RingOverviewResultComponent', () => {
  let component: RingOverviewResultComponent;
  let fixture: ComponentFixture<RingOverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [RingOverviewResultComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RingOverviewResultComponent);
    component = fixture.componentInstance;
    component.result = {
      Html: "<h1>Test</h1>"
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let h = fixture.nativeElement.querySelector('h1');
    let text = h.textContent;
    expect(text).toEqual("Test");
  });
});
