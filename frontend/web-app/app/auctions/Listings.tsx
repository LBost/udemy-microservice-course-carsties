'use client';

import { Auction, PagedResult } from '@/types';
import AuctionCard from './AuctionCard';
import AppPagination from '../components/AppPagination';
import { useEffect, useState } from 'react';
import { getData } from '../actions/auctionActions';
import Filters from './Filters';
import { useParamsStore } from '../hooks/useParamsStore';
import { useShallow } from 'zustand/shallow';
import queryString from 'query-string';
import EmptyFilter from '../components/EmptyFilter';

export default function Listings() {
  const [data, setData] = useState<PagedResult<Auction>>();
  const params = useParamsStore(
    useShallow((state) => ({
      pageNumber: state.pageNumber,
      pageSize: state.pageSize,
      searchTerm: state.searchTerm,
      orderBy: state.orderBy,
      filterBy: state.filterBy,
    }))
  );
  const setParams = useParamsStore((state) => state.setParams);
  const query = queryString.stringifyUrl(
    { url: '', query: params },
    { skipEmptyString: true }
  );

  function setPageNumber(pageNumber: number) {
    setParams({ pageNumber });
  }

  useEffect(() => {
    getData(query).then((data) => {
      setData(data);
    });
  }, [query]);

  if (!data) return <h3>Loading...</h3>;

  return (
    <>
      <Filters />
      {data.pageCount === 0 ? (
        <EmptyFilter showReset />
      ) : (
        <>
          <div className="grid grid-cols-4 gap-6">
            {data &&
              data.result.map((auction: Auction) => (
                <AuctionCard key={auction.id} auction={auction} />
              ))}
          </div>
          <div className="w-full justify-center flex mt-5">
            <AppPagination
              currentPage={params.pageNumber}
              pageCount={data.pageCount <= 0 ? 1 : data.pageCount}
              pageChanged={setPageNumber}
            />
          </div>
        </>
      )}
    </>
  );
}
