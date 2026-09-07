import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { KeyRound } from 'lucide-react';
import { Button, Input } from '@/shared/ui';
import { ROUTES } from '@/shared/config/routes';
import { AppError } from '@/shared/api/AppError';
import { useResolveSchoolCode } from '../api/useResolveSchoolCode';
import {
  formatSchoolEntryCode,
  isCompleteSchoolEntryCode,
  normalizeSchoolEntryCode,
} from '../lib/schoolEntryCode';

/** Backend xato kodini i18n kalitiga o'giradi — barcha "topilmadi" holatlari bitta generic xabar (`docs/08` 3a). */
function resolveErrorKey(error: unknown): string {
  if (error instanceof AppError) {
    if (error.code === 'SCHOOL_CODE_INVALID' || error.status === 404) {
      return 'account.login.school.errors.invalid';
    }
    if (error.code === 'RATE_LIMITED' || error.status === 429) {
      return 'account.login.school.errors.rateLimited';
    }
  }
  return 'account.login.school.errors.generic';
}

/**
 * `/kirish` dagi "Maktab uchun" kartasi — akkaunt ochmasdan, maktab bergan 8 belgili kod bilan
 * kirish. Kod `POST /api/public/schools/resolve-code` orqali `{ slug, accessToken }` ga
 * aylanadi va foydalanuvchi MAVJUD maktab oqimiga (`/t/:slug?k=`) o'tadi — u yerda anketa va
 * testlar allaqachon ishlaydi (`LandingPage` `k` ni o'qiydi).
 *
 * Kiritishda defis/bo'shliq/kichik harf qabul qilinadi; ko'rsatiladi `XXXX-XXXX`, yuboriladi
 * defissiz katta harfda (`normalizeSchoolEntryCode`).
 */
export function SchoolCodeCard() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const resolve = useResolveSchoolCode();
  const [isOpen, setIsOpen] = useState(false);
  const [code, setCode] = useState('');
  const [errorKey, setErrorKey] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Maydon FOYDALANUVCHI tugmani bosganidan keyin ochiladi — fokus shu harakatning davomi
  // (`autoFocus` atributi lint bo'yicha taqiqlangan: sahifa yuklanishida kutilmagan fokus beradi).
  useEffect(() => {
    if (isOpen) {
      inputRef.current?.focus();
    }
  }, [isOpen]);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!isCompleteSchoolEntryCode(code)) {
      setErrorKey('account.login.school.errors.required');
      return;
    }
    setErrorKey(null);
    resolve.mutate(code, {
      onSuccess: ({ slug, accessToken }) => {
        navigate(`${ROUTES.public.landing(slug)}?k=${encodeURIComponent(accessToken)}`);
      },
      onError: (error) => setErrorKey(resolveErrorKey(error)),
    });
  }

  return (
    <section
      aria-labelledby="school-code-heading"
      className="flex flex-col gap-4 rounded-4xl border border-line bg-paper-deep/50 p-6 text-left"
    >
      <div className="flex items-center gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-2xl bg-firuza-100 text-firuza-700">
          <KeyRound className="size-5" aria-hidden="true" focusable="false" />
        </span>
        <h2 id="school-code-heading" className="font-display text-xl font-extrabold text-ink">
          {t('account.login.school.title')}
        </h2>
      </div>
      <p className="text-[15px] leading-relaxed text-ink-soft">{t('account.login.school.lead')}</p>

      {isOpen ? (
        <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-3">
          <Input
            label={t('account.login.school.codeLabel')}
            placeholder={t('account.login.school.codePlaceholder')}
            value={formatSchoolEntryCode(code)}
            onChange={(event) => {
              setCode(normalizeSchoolEntryCode(event.target.value));
              setErrorKey(null);
            }}
            inputMode="text"
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            maxLength={9}
            ref={inputRef}
            className="font-mono tracking-widest uppercase"
            error={errorKey ? t(errorKey) : undefined}
          />
          <Button type="submit" isLoading={resolve.isPending} disabled={resolve.isPending}>
            {t('account.login.school.continueCta')}
          </Button>
        </form>
      ) : (
        <Button variant="secondary" onClick={() => setIsOpen(true)}>
          {t('account.login.school.openCta')}
        </Button>
      )}
    </section>
  );
}
