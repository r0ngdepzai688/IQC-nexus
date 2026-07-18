"use client";

import React, { createContext, useContext, useState, ReactNode } from "react";
import { dictionaries, Locale, Dictionary } from "../i18n/dictionaries";

interface LanguageContextType {
  locale: Locale;
  t: Dictionary;
  setLocale: (locale: Locale) => void;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

function getInitialLocale(): Locale {
  if (typeof window === "undefined") {
    return "vi";
  }

  const savedLocale = window.localStorage.getItem("app_locale");

  if (savedLocale === "en" || savedLocale === "vi") {
    return savedLocale;
  }

  return "vi";
}

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(getInitialLocale);

  const setLocale = (newLocale: Locale) => {
    setLocaleState(newLocale);
    window.localStorage.setItem("app_locale", newLocale);
  };

  const t = dictionaries[locale];

  return (
    <LanguageContext.Provider value={{ locale, t, setLocale }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const context = useContext(LanguageContext);

  if (context === undefined) {
    throw new Error("useLanguage must be used within a LanguageProvider");
  }

  return context;
}
