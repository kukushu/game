import argparse
import cmd
import json
from dataclasses import asdict
from pathlib import Path

from .experiments import compare, export_run, plot, run_until
from .model import Config, Economy


def config_file(path):
    config = Config(**json.loads(Path(path).read_text(encoding="utf-8-sig"))) if path else Config()
    config.validate()
    return config


def status(model):
    if not model.metrics:
        return f"Month {model.month}: initialized; cash={model.total_money()/100:.2f}"
    r = model.metrics[-1]
    return (f"Month {model.month}: employed={r['employment']:.1%}, food={r['food_coverage']:.1%}, "
            f"goods={r['goods_coverage']:.1%}, treasury={model.treasury/100:.2f}, "
            f"bridge={model.bridge_progress:.1f}/{model.policy.bridge_target:g}, cash error={r['money_error_cents']} cents")


class LabShell(cmd.Cmd):
    intro = "Economy debugger. Commands: step 12, status, policy, set income_tax 0.2, firm 0, household 0, why, save path.json, quit"
    prompt = "econ> "

    def __init__(self, model):
        super().__init__()
        self.model = model

    def onecmd(self, line):
        try:
            return super().onecmd(line)
        except (ValueError, IndexError, OSError, TypeError) as error:
            print(f"Error: {error}")

    def do_step(self, arg):
        "Advance N months (default 1)."
        count = int(arg or 1)
        if not 1 <= count <= 10000:
            raise ValueError("step must be between 1 and 10000")
        for _ in range(count):
            self.model.step()
        print(status(self.model))

    def do_status(self, arg):
        print(status(self.model))

    def do_policy(self, arg):
        print(json.dumps(asdict(self.model.policy), indent=2))

    def do_set(self, arg):
        "Apply a policy condition now: set training_seats 4."
        key, value = arg.split(maxsplit=1)
        self.model.intervene(key, json.loads(value))
        print(f"Applied {key}={value} at month {self.model.month}")

    def do_firm(self, arg):
        f = self.model.firms[int(arg)]
        print(json.dumps(asdict(f), indent=2))
        print("workers:", [h.id for h in self.model.households if h.job == f.id])

    def do_household(self, arg):
        h = self.model.households[int(arg)]
        print(json.dumps(asdict(h), indent=2))
        reachable = [f.id for f in self.model.firms if not f.closed and self.model.distance(h.district, f.district) <= self.model.config.max_commute and (f.sector == 'food' or h.skill >= 1)]
        print("qualified reachable employers (not necessarily hiring):", reachable)

    def do_why(self, arg):
        "Print diagnostic counts from the last month; not a causal estimator."
        print(json.dumps(self.model.reasons, indent=2))

    def do_save(self, arg):
        self.model.save(arg or "checkpoint.json")
        print("Saved")

    def do_quit(self, arg):
        return True

    do_EOF = do_quit


def main():
    parser = argparse.ArgumentParser(description="Isolated, auditable agent-based city economy laboratory")
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("run", "shell"):
        p = sub.add_parser(name)
        p.add_argument("--seed", type=int, default=1)
        p.add_argument("--config")
        p.add_argument("--resume")
        p.add_argument("--trace", action="store_true")
        if name == "run":
            p.add_argument("--months", type=int, default=72, help="Absolute end month, including resumed history")
            p.add_argument("--schedule")
            p.add_argument("--out", default="outputs/run")
    p = sub.add_parser("compare")
    p.add_argument("--seeds", default="1,2,3,4,5,6,7,8")
    p.add_argument("--months", type=int, default=72)
    p.add_argument("--warmup", type=int, default=12)
    p.add_argument("--tail", type=int, default=12)
    p.add_argument("--config")
    p.add_argument("--ablations", action="store_true")
    p.add_argument("--plot", action="store_true")
    p.add_argument("--out", default="outputs/comparison")
    p = sub.add_parser("plot")
    p.add_argument("out")
    p = sub.add_parser("defaults")
    p.add_argument("--out", default="config.json")
    args = parser.parse_args()
    try:
        if args.command == "defaults":
            Path(args.out).write_text(json.dumps(asdict(Config()), indent=2), encoding="utf-8")
            return
        if args.command == "plot":
            plot(args.out)
            return
        if args.command == "compare":
            seeds = [int(x) for x in args.seeds.split(",")]
            summaries, _ = compare(args.out, seeds, args.months, args.warmup, args.tail, config_file(args.config), args.ablations)
            for name in dict.fromkeys(r["scenario"] for r in summaries):
                group = [r for r in summaries if r["scenario"] == name]
                print(f"{name:20} employment={sum(r['employment'] for r in group)/len(group):.1%} food={sum(r['food_coverage'] for r in group)/len(group):.1%}")
            if args.plot:
                plot(args.out)
            print("Results:", Path(args.out).resolve())
            return
        if args.resume and args.config:
            parser.error("A saved snapshot includes its config; do not combine --resume and --config")
        model = (Economy.restore(json.loads(Path(args.resume).read_text(encoding="utf-8-sig")), args.trace)
                 if args.resume else Economy(args.seed, config_file(args.config), args.trace))
        if args.command == "shell":
            LabShell(model).cmdloop()
            return
        if args.months < model.month:
            raise ValueError("End month is before saved state")
        schedule = json.loads(Path(args.schedule).read_text(encoding="utf-8-sig")) if args.schedule else []
        run_until(model, args.months, schedule)
        export_run(model, args.out)
        print(status(model))
        print("Results:", Path(args.out).resolve())
    except (ValueError, OSError, TypeError, KeyError) as error:
        parser.error(str(error))


if __name__ == "__main__":
    main()
