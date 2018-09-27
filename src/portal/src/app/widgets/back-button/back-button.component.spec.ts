import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BackButtonComponent } from './back-button.component';
import { Location } from '@angular/common';
import { MaterialsModule } from '../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

const locationStub = {
  back: jasmine.createSpy('back')
}

fdescribe('BackButtonComponent', () => {
  let component: BackButtonComponent;
  let fixture: ComponentFixture<BackButtonComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [BackButtonComponent],
      providers: [
        { provide: Location, useValue: locationStub }
      ],
      imports: [MaterialsModule, NoopAnimationsModule]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BackButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call goBack', () => {
    component.goBack();
    const location = fixture.debugElement.injector.get(Location);
    expect(location.back).toHaveBeenCalled();
  })
});
