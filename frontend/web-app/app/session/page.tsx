import { auth } from '@/auth';
import Headings from '../components/Headings';
import AuthTest from './AuthTest';

export default async function Session() {
  const session = await auth();
  return (
    <div>
      <Headings title="Session dashboard" centered />
      <div className="bg-blue-200 border border-blue-500 p-2 rounded-lg">
        <h3 className="text-lg">Session data</h3>
        <pre className="whitespace-pre-wrap break-all">
          {JSON.stringify(session, null, 2)}
        </pre>
      </div>
      <div className="mt-4">
        <AuthTest />
      </div>
    </div>
  );
}
