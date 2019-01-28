import { of } from 'rxjs/observable/of';
import { AuthService } from './auth.service';

fdescribe('AuthService', () => {
  let apiServiceSpy;
  let authService: AuthService;
  let userServiceSpy;
  beforeEach(() => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['getUserInfo']);
    userServiceSpy.getUserInfo.and.returnValue(
      of({
        status: '200',
        body: [
          { user_id: 'test user' }
        ]
      })
    );
    apiServiceSpy = jasmine.createSpy('ApiService');
    apiServiceSpy.user = userServiceSpy;
    authService = new AuthService(apiServiceSpy);
  });

  it('should be created', () => {
    expect(authService).toBeTruthy();
  });

  it('#getUserInfo should set username', () => {
    authService.getUserInfo();
    expect(authService.username).toEqual('test user');
  });
});
