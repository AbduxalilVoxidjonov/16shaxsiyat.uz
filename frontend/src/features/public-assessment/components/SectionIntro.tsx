export interface SectionIntroProps {
  title: string;
  description?: string | null;
}

/**
 * Bo'lim sarlavhasi va ixtiyoriy tavsifi (`docs/18` §6.2) — bo'lim-qadam rejimida
 * (`TestPage`, keyingi to'lqin) bitta ko'rinadigan bo'lim ekranining yuqorisida ko'rsatiladi.
 * `dangerouslySetInnerHTML` ISHLATILMAYDI — matn oddiy `<p>` sifatida render qilinadi
 * (`CLAUDE.md` 12-qoida).
 */
export function SectionIntro({ title, description }: SectionIntroProps) {
  return (
    <div className="mb-6 text-center">
      <h2 className="font-display text-2xl font-extrabold text-balance text-ink sm:text-3xl">
        {title}
      </h2>
      {description && <p className="mt-2 text-ink-soft">{description}</p>}
    </div>
  );
}
