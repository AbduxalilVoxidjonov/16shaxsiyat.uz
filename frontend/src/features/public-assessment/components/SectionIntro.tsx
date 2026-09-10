export interface SectionIntroProps {
  title: string;
  description?: string | null;
  /**
   * `TestPage` bo'lim-qadam rejimida bo'lim almashganda shu tugunga fokus o'tkazadi (a11y,
   * `docs/18` §6.2: "bo'lim almashganda fokus yangi bo'lim sarlavhasiga ko'chsin"). Shu sabab
   * sarlavha `tabIndex={-1}` bilan dasturiy fokusga ochiq.
   */
  headingRef?: (node: HTMLHeadingElement | null) => void;
}

/**
 * Bo'lim sarlavhasi va ixtiyoriy tavsifi (`docs/18` §6.2) — bo'lim-qadam rejimida
 * (`TestPage`, keyingi to'lqin) bitta ko'rinadigan bo'lim ekranining yuqorisida ko'rsatiladi.
 * `dangerouslySetInnerHTML` ISHLATILMAYDI — matn oddiy `<p>` sifatida render qilinadi
 * (`CLAUDE.md` 12-qoida).
 */
export function SectionIntro({ title, description, headingRef }: SectionIntroProps) {
  return (
    <div className="mb-6 text-center">
      <h2
        ref={headingRef}
        tabIndex={-1}
        className="font-display text-2xl font-extrabold text-balance text-ink outline-none sm:text-3xl"
      >
        {title}
      </h2>
      {description && <p className="mt-2 text-ink-soft">{description}</p>}
    </div>
  );
}
