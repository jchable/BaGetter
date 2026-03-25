import { useEffect, useRef, useState } from "react";

export function useSSE<T>(url: string | null) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const sourceRef = useRef<EventSource | null>(null);

  useEffect(() => {
    if (!url) {
      setIsConnected(false);
      return;
    }

    const source = new EventSource(url);
    sourceRef.current = source;

    source.onopen = () => setIsConnected(true);

    source.onmessage = (event) => {
      try {
        setData(JSON.parse(event.data) as T);
      } catch {
        setError("Failed to parse SSE data");
      }
    };

    source.onerror = () => {
      setIsConnected(false);
      source.close();
    };

    return () => {
      source.close();
      setIsConnected(false);
    };
  }, [url]);

  return { data, error, isConnected };
}
