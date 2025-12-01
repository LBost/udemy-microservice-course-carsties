'use server';

import { auth } from '@/auth';

export async function getCurrentUser() {
  try {
    const session = await auth();
    if (!session) return;
    return session.user;
  } catch (error) {
    console.error(error);
    return null;
  }
}
