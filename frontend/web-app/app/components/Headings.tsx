type Props = {
  title: string;
  subTitle?: string;
  centered?: boolean;
};

export default function Headings(props: Props) {
  return (
    <div className={props.centered ? 'text-center' : 'text-start'}>
      <h1 className="text-2xl font-bold">{props.title}</h1>
      {props.subTitle && (
        <p className="font-light text-neutral-500">{props.subTitle}</p>
      )}
    </div>
  );
}
