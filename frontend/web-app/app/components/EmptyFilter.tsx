import { Button } from 'flowbite-react';
import { useParamsStore } from '../hooks/useParamsStore';
import Headings from './Headings';

type Props = {
  title?: string;
  subTitle?: string;
  showReset?: boolean;
};

export default function EmptyFilter({
  title = 'No matches for this filter',
  subTitle = 'Try changing the filter or search term',
  showReset,
}: Props) {
  const reset = useParamsStore((state) => state.reset);

  return (
    <div className="flex flex-col items-center justify-center h-[40vh] shadow-lg">
      <Headings title={title} subTitle={subTitle} centered />
      <div className="mt-4">
        {showReset && <Button onClick={reset}>Reset filter</Button>}
      </div>
    </div>
  );
}
