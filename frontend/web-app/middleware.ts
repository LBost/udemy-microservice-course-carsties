import { auth } from '@/auth';
export { auth as middleware } from '@/auth';

export const config = {
  matcher: ['/session'],
  pages: {
    signIn: '/api/auth/signin',
  },
};

export default auth((req) => {
  // req.auth
  console.log('auth req: ' + req);
});
