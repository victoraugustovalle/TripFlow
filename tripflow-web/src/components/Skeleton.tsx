export function SkeletonLines({ count = 3 }: { count?: number }) {
  return (
    <div className="flex flex-col divide-y divide-cream-200" aria-hidden="true">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex items-center justify-between py-3">
          <div className="flex flex-col gap-2">
            <div className="skeleton-shimmer h-3.5 w-40 rounded" />
            <div className="skeleton-shimmer h-3 w-24 rounded" />
          </div>
          <div className="skeleton-shimmer h-3.5 w-16 rounded" />
        </div>
      ))}
    </div>
  );
}

export function SkeletonCards({ count = 4 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2" aria-hidden="true">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="h-40 overflow-hidden rounded-2xl border border-cream-300 bg-white">
          <div className="skeleton-shimmer h-20" />
          <div className="flex flex-col gap-2 p-4">
            <div className="skeleton-shimmer h-4 w-32 rounded" />
            <div className="skeleton-shimmer h-3 w-20 rounded" />
          </div>
        </div>
      ))}
    </div>
  );
}
