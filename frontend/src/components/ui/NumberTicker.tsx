"use client";

import { useEffect, useState } from "react";
import { motion, useSpring, useTransform } from "framer-motion";

interface NumberTickerProps {
  value: number;
  direction?: "up" | "down";
  className?: string;
}

export default function NumberTicker({
  value,
  direction = "up" /* eslint-disable-line @typescript-eslint/no-unused-vars */,
  className,
}: NumberTickerProps) {
  const [hasHydrated, setHasHydrated] = useState(false);
  const springValue = useSpring(0, {
    stiffness: 50,
    damping: 20,
    restDelta: 0.001,
  });

  const displayValue = useTransform(springValue, (current) =>
    Math.round(current).toLocaleString()
  );

  useEffect(() => {
    setHasHydrated(true); // eslint-disable-line react-hooks/set-state-in-effect
  }, []);

  useEffect(() => {
    if (hasHydrated) {
      springValue.set(value);
    }
  }, [springValue, value, hasHydrated]);

  if (!hasHydrated) {
    return <span className={className}>0</span>;
  }

  return (
    <motion.span className={className}>
      {displayValue}
    </motion.span>
  );
}

