import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { OverviewResultComponent } from './overview-result.component';

fdescribe('OverviewResultComponent', () => {
  let component: OverviewResultComponent;
  let fixture: ComponentFixture<OverviewResultComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [OverviewResultComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(OverviewResultComponent);
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
