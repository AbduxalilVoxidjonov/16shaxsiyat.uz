import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { cn } from '@/shared/lib/cn';
import { GirihStar } from './GirihStar';

export interface LogoProps {
  className?: string;
  /** `true` bo'lsa logotip bosh sahifaga havola bo'ladi (header uchun). Standart: `false`. */
  asLink?: boolean;
}

/**
 * Sayt logotipi — gradientli girih nishoni va yonida brend nomi.
 *
 * Brend nomi `common.json` dagi `app.name` kalitidan olinadi (CLAUDE.md "Brend" bo'limi:
 * foydalanuvchiga ko'rinadigan matnda "Shaxsiyat" ishlatiladi).
 */
export function Logo({ className, asLink = false }: LogoProps) {
  const { t } = useTranslation();
  const appName = t('app.name');

  const mark = (
    <>
      <span className="relative grid size-10 place-items-center rounded-[28%] bg-linear-to-br from-firuza-400 to-firuza-700 shadow-glow transition-transform duration-300 group-hover:rotate-45">
        <GirihStar className="absolute inset-[16%] text-white/40" strokeWidth={2} />
        <GirihStar
          className="absolute inset-[34%] text-white/25"
          strokeWidth={1.5}
          withCircle={false}
        />
      </span>
      <span className="font-display text-[19px] font-extrabold tracking-tight text-ink">
        {appName}
      </span>
    </>
  );

  const classes = cn('group inline-flex items-center gap-2.5', className);

  if (asLink) {
    return (
      <Link to="/" className={classes} aria-label={`${appName} — bosh sahifa`}>
        {mark}
      </Link>
    );
  }

  return <span className={classes}>{mark}</span>;
}
